/** @jsx React.DOM */ 

var React = require('react');
var AppDispatcher = require('../dispatcher/AppDispatcher');
var AppActions = require('../actions/AppActions');
var SearchStore = require('../stores/SearchStore');
var PreviousSearchEntry = require('./PreviousSearchEntry.react');

var PreviousSearches = React.createClass({

    getInitialState: function() {
        return {previous: []};
    },

    componentWillMount: function() {
        SearchStore.addChangeListener(this._onChange);
    },

    componentDidUnmount: function() {
        SearchStore.removeChangeListener(this._onChange);
    },

    render: function() {

        var rows = _.map(this.state.previous, function(x) {
            return <li><PreviousSearchEntry searchTerm={x.searchTerm} newItems={x.newItems}/></li>
        });

        return (
        <div>
            Previous searches: {this.state.previous.length}
            <ul>
                {rows}
            </ul>
        </div>
        );
    },

    _onChange: function() {
        this.setState({previous: SearchStore.getPreviousSearches()});
    }

});

module.exports = PreviousSearches;
