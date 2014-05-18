/** @jsx React.DOM */

var Search = React.createClass({
    getInitialState: function() {
	return {searchText: "" };
    },
    handleSubmit: function() {
	//console.log(this.refs.searchInput.getDOMNode().value);
	var val = this.refs.searchInput.getDOMNode().value;
	this.props.onUserInput(val);
	return false;
    },
    handleChange: function() {
	var val = this.refs.searchInput.getDOMNode().value;
	console.log(val);
        this.setState({searchText: val});
    },
    render: function() {
	if (this.state.searchText == "dingo")
	    return (
	    <form onSubmit={this.handleSubmit}>
		<h3>Search</h3>
		<input type="text" 
	               value={this.state.searchText} 
	               ref="searchInput" 
	               onChange={this.handleChange}/>
	    </form>
	    )
	else
	{
	return (
	    <form onSubmit={this.handleSubmit}>
		<h3>Search</h3>
		<input type="text" 
	               value={this.state.searchText} 
	               ref="searchInput" 
	               onChange={this.handleChange}/>
		<button>Search</button>
	    </form>
	);
	}
    }
});

var SearchItemRow = React.createClass({
    render: function() {
	return (
	    <div className="item">
		<img className="item-box-left" src={this.props.thumbnail} />
		<div className="item-box-right">{this.props.text}</div>
	    </div>	
	);
    }
});

var SearchTable = React.createClass({
    getInitialState: function() {
	return {searchResults: []};
    },
    onUserInput: function(text) {
	console.log("Searching for " + text);
	$.get("http://localhost:3000/search-one?name=" + text, function(data) {
	    console.dir(data);
	    this.setState({searchResults: data});
	}.bind(this));

	return false;
    },
    render: function() { 
	var rows = [];
	this.state.searchResults.forEach(function(res) {
	    rows.push(<SearchItemRow thumbnail={res.thumbnail} 
		                     text={res.text}/>);
	});
	return (
	    <div className="row">
		<Search onUserInput={this.onUserInput}/>
		<h3>Search Results</h3>
		<div className="col-xs-12 col-sm-8 col-md-8 column-1">{rows}</div>
	    </div>
	);
    }
});

var MonitorItem = React.createClass({
    render: function() {
	return (
	    <div className="monitor">
		<div className="search-term">Monitoring {this.props.searchTerm}</div>
		<div className="number-of-new-items">{this.props.numberNewItems}</div> new items.
	    </div>	
	);
    }
});

var MonitorTable = React.createClass({
    render: function() {
	var rows = _.map(this.props.monitors, function(item) {
	    console.log("item " + item);
	    return <MonitorItem searchTerm={item} 
		                   numberNewItems={item.numberNewItems} />
	});
	return (
	    <div className="row">
		<h3>Monitors</h3>
		<div className="col-xs-12 col-sm-12 col-md-4 column-2">{rows}</div>
            </div>
	);
    }
});

var App = React.createClass({
    getInitialState: function() {
	$.get("http://localhost:3000/user/" + this.props.data.userId, function(data) {
	    console.dir(data);
 	    this.setState({monitors: data.monitors});
	}.bind(this));

	return {monitors: []};
    },
    render: function() {
	return (
	    <div className="row">
		<div className="col-xs-12 col-sm-12 col-md-4 column-2">
		   <MonitorTable monitors={this.state.monitors} />
		</div>
            </div>
	);
    }
})

var data = {
    monitorItems: [{"searchTerm": "fisk", "numberNewItems": 4},
		   {"searchTerm": "dingo", "numberNewItems": 2}],
    searchItems: [],
    userId: 123,
};

var root = React.renderComponent(<App data={data} />, 
				 document.getElementById('app'));


//var root = React.renderComponent(<MonitorTable monitors={data.monitorItems} />, 
//				 document.getElementById('monitor'));

//React.renderComponent(<SearchTable searchResults={data.searchItems} />, 
//		      document.getElementById('app'));


var SearchTest = React.createClass({
    onClick: function() {
	console.log("Clicked");

	$.get("http://localhost:3000/search-one?name=stokke", function(data) {
	    console.dir(data);
	});

	
    },

    render: function() {
	return (
	    <button onClick={this.onClick}>Query</button>
	);
    }
});
React.renderComponent(<SearchTest />, document.getElementById('example'));






