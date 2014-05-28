/** @jsx React.DOM */ 

var React = require('react');
var ReactPropTypes = React.PropTypes;
var SearchTextInput = require('./SearchTextInput.react');
var GoogleLoginButton = require('./GoogleLoginButton.react');
var AppActions = require('../actions/AppActions');
var UserStore = require('../stores/UserStore');

var NavigationBar = React.createClass({

    getInitialState: function(){
	return { user: UserStore.getUser() };
    },

    componentDidMount: function() {
	UserStore.addChangeListener(this._onChange);
    },

    componentWillUnmount: function() {
	UserStore.removeChangeListener(this._onChange);
    },

    onSubmit: function(value) {
	AppActions.search(value);
    },

    render: function() {
	return (
	<div>
		{this.state.username}
	    <img src={this.state.userImage} />
	    <GoogleLoginButton  />
	    <SearchTextInput className="dingo" onSubmit={this.onSubmit} />
	</div>
	);
    },

    _onChange: function() {
	this.setState(UserStore.getUser());
    }
});

module.exports = NavigationBar;
